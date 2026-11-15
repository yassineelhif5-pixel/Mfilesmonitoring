import { CommonModule } from '@angular/common';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';

interface LoginResult { username: string; mustChangePassword: boolean; }
interface Vault {
  id: string; name: string; vaultGuid: string; cloudHost: string;
  organization: string; environment: string; monitoringEnabled: boolean;
  authenticationMode: string;
  lastHealthCheck?: {
    checkedAtUtc: string; status: string; durationMs: number;
    authenticationStatus: string; authenticatedAccountName?: string;
    serverVersion?: string; capabilitiesSummary?: string; message: string;
  } | null;
}
interface Dashboard {
  totalVaults: number; healthyVaults: number; warningVaults: number;
  failedVaults: number; unknownVaults: number; activeIncidents: number;
  vaultsCheckedLast7Days: number;
  dailyStatistics: Array<{ dateUtc: string; createdCount: number; createdCountExact: boolean; totalObjectCount: number; totalObjectCountExact: boolean }>;
}
interface DatabaseConfig { configured: boolean; provider: string; server?: string; database?: string; authentication?: string; encrypt?: string; message?: string; }
interface DatabaseTest { success: boolean; code: string; message: string; explanation: string; server?: string; database?: string; technicalMessage?: string; }
interface Incident {
  id: string; vaultId: string; vaultName: string; category: string;
  severity: string; status: string; title: string; description: string;
  firstSeenUtc: string; lastSeenUtc: string; occurrences: number; lastErrorCode?: string;
}

@Component({
  selector: 'mfcmp-root',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="shell">
      <header class="topbar">
        <div class="brand-mark">M</div>
        <div class="brand-copy"><strong>M-Files Cloud Monitoring</strong><span>Support Operations Portal</span></div>
        <div class="spacer"></div><span class="readonly">READ ONLY</span>
        <span class="user" *ngIf="authenticated">{{ username }}</span>
        <button class="ghost" *ngIf="authenticated" (click)="logout()">Déconnexion</button>
      </header>

      <main class="content" *ngIf="!authenticated">
        <section class="login-card">
          <div class="eyebrow">MFCMP · CLOUD MONITORING</div><h1>Connexion au portail</h1>
          <p class="muted">Accès réservé à l'équipe de supervision M-Files.</p>
          <form (ngSubmit)="login()">
            <label for="username">Identifiant</label><input id="username" name="username" [(ngModel)]="loginName" autocomplete="username" required>
            <label for="password">Mot de passe</label><input id="password" name="password" type="password" [(ngModel)]="loginPassword" autocomplete="current-password" required>
            <button class="primary full" type="submit" [disabled]="busy">Se connecter</button>
          </form>
          <p class="error" *ngIf="errorMessage">{{ errorMessage }}</p>
          <p class="muted small">Le mot de passe initial doit être changé à la première connexion.</p>
        </section>
      </main>

      <main class="content" *ngIf="authenticated && mustChangePassword">
        <section class="login-card">
          <div class="eyebrow">SÉCURITÉ DU COMPTE</div><h1>Changer le mot de passe</h1>
          <p class="muted">Remplace le mot de passe initial avant d'accéder au portail.</p>
          <form (ngSubmit)="changePassword()">
            <label for="currentPassword">Mot de passe actuel</label><input id="currentPassword" name="currentPassword" type="password" [(ngModel)]="currentPassword" required>
            <label for="newPassword">Nouveau mot de passe</label><input id="newPassword" name="newPassword" type="password" [(ngModel)]="newPassword" required>
            <label for="confirmPassword">Confirmation</label><input id="confirmPassword" name="confirmPassword" type="password" [(ngModel)]="confirmPassword" required>
            <button class="primary full" type="submit" [disabled]="busy">Enregistrer</button>
          </form>
          <p class="error" *ngIf="errorMessage">{{ errorMessage }}</p>
        </section>
      </main>

      <main class="content" *ngIf="authenticated && !mustChangePassword">
        <div class="page-heading">
          <div><div class="eyebrow">OVERVIEW</div><h1>Supervision des coffres</h1><p class="muted">Disponibilité, authentification, incidents et activité documentaire M-Files Cloud.</p></div>
          <button class="primary" (click)="refreshAll()" [disabled]="busy">↻ Actualiser</button>
        </div>

        <section class="kpi-grid">
          <article class="kpi"><span>Coffres surveillés</span><strong>{{ dashboard?.totalVaults ?? 0 }}</strong><small>{{ dashboard?.vaultsCheckedLast7Days ?? 0 }} contrôlés récemment</small></article>
          <article class="kpi"><span>Healthy</span><strong class="good">{{ dashboard?.healthyVaults ?? 0 }}</strong><small>Connexion et session validées</small></article>
          <article class="kpi"><span>Warning / Unknown</span><strong class="warn">{{ (dashboard?.warningVaults ?? 0) + (dashboard?.unknownVaults ?? 0) }}</strong><small>Configuration ou données manquantes</small></article>
          <article class="kpi"><span>Incidents actifs</span><strong class="bad">{{ dashboard?.activeIncidents ?? 0 }}</strong><small>{{ dashboard?.failedVaults ?? 0 }} coffre(s) en échec</small></article>
        </section>

        <section class="notice"><strong>Supervision en lecture seule</strong><span>Les appels M-Files exécutés par le portail sont limités aux diagnostics, à l'authentification/session et aux recherches de comptage. Aucun objet n'est créé, modifié ou supprimé.</span></section>

        <section class="panel">
          <div class="panel-heading"><div><h2>Coffres et état de connexion</h2><p class="muted small">Health checks automatiques, fréquence configurée par coffre. Le compte technique doit disposer uniquement des droits de lecture requis.</p></div><span class="count">{{ vaults.length }} coffre(s)</span></div>
          <div class="empty" *ngIf="vaults.length === 0">Aucun coffre configuré. Enregistre un premier coffre ci-dessous.</div>
          <div class="table-wrap" *ngIf="vaults.length > 0">
            <table><thead><tr><th>Coffre</th><th>Env.</th><th>Authentification</th><th>État</th><th>Version</th><th>Temps</th><th></th></tr></thead>
              <tbody><tr *ngFor="let vault of vaults">
                <td><strong>{{ vault.name }}</strong><span class="sub">{{ vault.cloudHost }}</span></td>
                <td>{{ vault.environment }}</td><td>{{ vault.lastHealthCheck?.authenticationStatus || vault.authenticationMode }}</td>
                <td><span class="status" [class.ok]="vault.lastHealthCheck?.status === 'HEALTHY'" [class.fail]="vault.lastHealthCheck?.status?.includes('FAILED') || vault.lastHealthCheck?.status === 'CONNECTION_ERROR'">{{ vault.lastHealthCheck?.status || 'NOT_TESTED' }}</span></td>
                <td>{{ vault.lastHealthCheck?.serverVersion || '—' }}</td>
                <td>{{ vault.lastHealthCheck?.durationMs ?? '—' }}<span *ngIf="vault.lastHealthCheck"> ms</span></td>
                <td><button class="small-button" (click)="testConnection(vault)" [disabled]="busy">Tester</button></td>
              </tr></tbody>
            </table>
          </div>
        </section>

        <section class="panel">
          <div class="panel-heading"><div><h2>Activité documentaire — 7 derniers jours</h2><p class="muted small">Comptage des objets créés (propriété système Created) et total des objets non supprimés. Les indicateurs précisent si le comptage est exact selon M-Files.</p></div></div>
          <div class="empty" *ngIf="!dashboard?.dailyStatistics?.length">Pas encore de données. Configure une identité de supervision M-Files pour activer la collecte.</div>
          <div class="table-wrap" *ngIf="dashboard?.dailyStatistics?.length">
            <table><thead><tr><th>Date UTC</th><th>Objets créés</th><th>Total objets</th><th>Collecte</th></tr></thead>
              <tbody><tr *ngFor="let day of dashboard?.dailyStatistics">
                <td>{{ day.dateUtc }}</td><td>{{ day.createdCount | number }} <span class="sub" *ngIf="!day.createdCountExact">Comptage non exact (limite M-Files)</span></td>
                <td>{{ day.totalObjectCount | number }} <span class="sub" *ngIf="!day.totalObjectCountExact">Comptage non exact (limite M-Files)</span></td>
                <td><span class="status ok">AVAILABLE</span></td>
              </tr></tbody>
            </table>
          </div>
        </section>

        <section class="panel">
          <div class="panel-heading"><div><h2>Incidents</h2><p class="muted small">Incidents générés par les contrôles de connexion, d'authentification et d'accès au coffre.</p></div><span class="count">{{ incidents.length }} affiché(s)</span></div>
          <div class="empty" *ngIf="incidents.length === 0">Aucun incident enregistré.</div>
          <div class="table-wrap" *ngIf="incidents.length">
            <table><thead><tr><th>Incident</th><th>Coffre</th><th>Priorité</th><th>État</th><th>Occurrences</th><th>Dernière détection</th><th>Action</th></tr></thead>
              <tbody><tr *ngFor="let incident of incidents">
                <td><strong>{{ incident.title }}</strong><span class="sub">{{ incident.description }}</span></td>
                <td>{{ incident.vaultName }}</td><td><span class="status" [class.fail]="incident.severity === 'P1'">{{ incident.severity }}</span></td>
                <td>{{ incident.status }}</td><td>{{ incident.occurrences }}</td><td>{{ incident.lastSeenUtc | date:'short' }}</td>
                <td><select [ngModel]="incident.status" (ngModelChange)="updateIncident(incident, $event)" aria-label="État incident"><option>NEW</option><option>ACKNOWLEDGED</option><option>IN_PROGRESS</option><option>RESOLVED</option><option>CLOSED</option></select></td>
              </tr></tbody>
            </table>
          </div>
        </section>

        <section class="panel">
          <div class="panel-heading"><div><h2>Base de données du portail</h2><p class="muted small">SQL Server utilisé pour stocker les coffres, contrôles, statistiques, incidents et comptes du portail.</p></div><span class="count">{{ databaseConfig?.provider || 'SQL Server' }}</span></div>
          <div class="db-config" *ngIf="databaseConfig">
            <div><span class="sub">Serveur</span><strong>{{ databaseConfig.server || 'Non configuré' }}</strong></div>
            <div><span class="sub">Base</span><strong>{{ databaseConfig.database || 'Non configurée' }}</strong></div>
            <div><span class="sub">Authentification</span><strong>{{ databaseConfig.authentication || '—' }}</strong></div>
            <div><span class="sub">Chiffrement</span><strong>{{ databaseConfig.encrypt || '—' }}</strong></div>
          </div>
          <p class="muted small" *ngIf="!databaseConfig?.configured">ConnectionStrings:DefaultConnection n'est pas configurée dans le backend.</p>
          <div class="form-action"><button class="primary" (click)="testDatabase()" [disabled]="databaseBusy">Tester la connexion SQL</button></div>
          <div class="db-result" *ngIf="databaseTest" [class.db-ok]="databaseTest.success" [class.db-error]="!databaseTest.success">
            <strong>{{ databaseTest.message }}</strong>
            <p>{{ databaseTest.explanation }}</p>
            <small *ngIf="databaseTest.code">Code : {{ databaseTest.code }}</small>
            <small *ngIf="databaseTest.server">Serveur : {{ databaseTest.server }} · Base : {{ databaseTest.database }}</small>
            <small *ngIf="databaseTest.technicalMessage">Détail technique : {{ databaseTest.technicalMessage }}</small>
          </div>
        </section>

        <section class="panel">
          <h2>Ajouter un coffre M-Files Cloud</h2>
          <p class="muted small">Les secrets ne sont jamais enregistrés en clair dans SQL. La référence doit pointer vers un secret Azure Key Vault contenant un JSON Credentials ou Token.</p>
          <form class="vault-form" (ngSubmit)="addVault()">
            <div><label for="vaultName">Nom du coffre</label><input id="vaultName" name="vaultName" [(ngModel)]="newVault.name" required></div>
            <div><label for="vaultGuid">Vault GUID</label><input id="vaultGuid" name="vaultGuid" [(ngModel)]="newVault.vaultGuid" required></div>
            <div><label for="cloudHost">Cloud host (HTTPS)</label><input id="cloudHost" name="cloudHost" [(ngModel)]="newVault.cloudHost" placeholder="https://tenant.cloudvault.m-files.com" required></div>
            <div><label for="organization">Organisation</label><input id="organization" name="organization" [(ngModel)]="newVault.organization"></div>
            <div><label for="environment">Environnement</label><select id="environment" name="environment" [(ngModel)]="newVault.environment"><option>DEV</option><option>TEST</option><option>UAT</option><option>PREPROD</option><option>PROD</option></select></div>
            <div><label for="authMode">Authentification M-Files</label><select id="authMode" name="authMode" [(ngModel)]="newVault.authenticationMode"><option value="None">Non configurée</option><option value="Credentials">M-Files username/password</option><option value="Token">Token / plugin SSO</option></select></div>
            <div class="wide" *ngIf="newVault.authenticationMode !== 'None'"><label for="secretUri">Azure Key Vault Secret URI</label><input id="secretUri" name="secretUri" [(ngModel)]="newVault.credentialSecretUri" placeholder="https://vaultname.vault.azure.net/secrets/mfiles-monitoring"></div>
            <div *ngIf="newVault.authenticationMode === 'Token'"><label for="plugin">Authentication plugin (optionnel)</label><input id="plugin" name="plugin" [(ngModel)]="newVault.authenticationPlugin"></div>
            <div *ngIf="newVault.authenticationMode === 'Token'"><label for="scope">Authentication scope (optionnel)</label><input id="scope" name="scope" [(ngModel)]="newVault.authenticationScope"></div>
            <div><label for="healthInterval">Health check (secondes)</label><input id="healthInterval" name="healthInterval" type="number" min="30" max="3600" [(ngModel)]="newVault.healthCheckIntervalSeconds"></div>
            <div><label for="metricsInterval">Collecte métriques (minutes)</label><input id="metricsInterval" name="metricsInterval" type="number" min="5" max="1440" [(ngModel)]="newVault.metricsCollectionIntervalMinutes"></div>
            <div class="form-action"><button class="primary" type="submit" [disabled]="busy">Ajouter le coffre</button></div>
          </form>
        </section>
        <p class="error" *ngIf="errorMessage">{{ errorMessage }}</p>
      </main>
      <footer>MFCMP · M-Files Cloud only · Read-only monitoring</footer>
    </div>
  `
})
export class AppComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private csrfToken = '';
  authenticated = false; mustChangePassword = false; username = '';
  loginName = 'admin'; loginPassword = 'admin';
  currentPassword = 'admin'; newPassword = ''; confirmPassword = '';
  busy = false; errorMessage = ''; vaults: Vault[] = [];
  dashboard: Dashboard | null = null; incidents: Incident[] = [];
  databaseConfig: DatabaseConfig | null = null; databaseTest: DatabaseTest | null = null; databaseBusy = false;
  newVault = {
    name: '', vaultGuid: '', cloudHost: '', organization: '', environment: 'DEV',
    authenticationMode: 'None', credentialSecretUri: '', authenticationPlugin: '',
    authenticationScope: '', healthCheckIntervalSeconds: 60, metricsCollectionIntervalMinutes: 15
  };

  ngOnInit(): void {
    this.http.get<{ requestToken: string }>('/api/auth/csrf', { withCredentials: true }).subscribe({
      next: result => { this.csrfToken = result.requestToken; this.restoreSession(); },
      error: () => this.errorMessage = 'Impossible de contacter le backend. Vérifie que l’API est démarrée.'
    });
  }
  private headers(): HttpHeaders { return new HttpHeaders({ 'X-CSRF-TOKEN': this.csrfToken }); }
  private restoreSession(): void {
    this.http.get<LoginResult>('/api/auth/me', { withCredentials: true }).subscribe({
      next: r => { this.authenticated = true; this.username = r.username; this.mustChangePassword = r.mustChangePassword; if (!this.mustChangePassword) this.refreshAll(); },
      error: () => this.authenticated = false
    });
  }
  login(): void {
    this.busy = true; this.errorMessage = '';
    this.http.post<LoginResult>('/api/auth/login', { username: this.loginName, password: this.loginPassword }, { headers: this.headers(), withCredentials: true }).subscribe({
      next: r => { this.authenticated = true; this.username = r.username; this.mustChangePassword = r.mustChangePassword; this.busy = false; if (!this.mustChangePassword) this.refreshAll(); },
      error: e => { this.errorMessage = e.error?.message || 'Échec de connexion.'; this.busy = false; }
    });
  }
  changePassword(): void {
    this.busy = true; this.errorMessage = '';
    this.http.post('/api/auth/change-password', { currentPassword: this.currentPassword, newPassword: this.newPassword, confirmPassword: this.confirmPassword }, { headers: this.headers(), withCredentials: true }).subscribe({
      next: () => { this.mustChangePassword = false; this.busy = false; this.refreshAll(); },
      error: e => { this.errorMessage = e.error?.errors?.join(' ') || e.error?.message || 'Impossible de modifier le mot de passe.'; this.busy = false; }
    });
  }
  refreshAll(): void { this.loadVaults(); this.loadDashboard(); this.loadIncidents(); this.loadDatabaseConfig(); }
  loadDatabaseConfig(): void { this.http.get<DatabaseConfig>('/api/configuration/database', { withCredentials: true }).subscribe({ next: r => this.databaseConfig = r, error: () => this.errorMessage = 'Impossible de lire la configuration SQL.' }); }
  testDatabase(): void { this.databaseBusy = true; this.databaseTest = null; this.http.post<DatabaseTest>('/api/configuration/database/test', {}, { headers: this.headers(), withCredentials: true }).subscribe({ next: r => { this.databaseTest = r; this.databaseBusy = false; this.loadDatabaseConfig(); }, error: e => { this.databaseTest = { success: false, code: 'API_ERROR', message: 'Le test SQL n’a pas pu être exécuté.', explanation: e.error?.message || 'Vérifie que le backend est démarré et que la session est valide.' }; this.databaseBusy = false; } }); }
  loadVaults(): void {
    this.http.get<Vault[]>('/api/vaults', { withCredentials: true }).subscribe({
      next: r => this.vaults = r,
      error: () => this.errorMessage = 'Impossible de charger les coffres.'
    });
  }
  loadDashboard(): void {
    this.http.get<Dashboard>('/api/monitoring/dashboard', { withCredentials: true }).subscribe({
      next: r => this.dashboard = r,
      error: () => this.errorMessage = 'Impossible de charger le dashboard.'
    });
  }
  loadIncidents(): void {
    this.http.get<Incident[]>('/api/monitoring/incidents', { withCredentials: true }).subscribe({
      next: r => this.incidents = r,
      error: () => this.errorMessage = 'Impossible de charger les incidents.'
    });
  }
  addVault(): void {
    this.busy = true; this.errorMessage = '';
    this.http.post('/api/vaults', {
      ...this.newVault, monitoringEnabled: true,
      credentialSecretUri: this.newVault.credentialSecretUri || null,
      authenticationPlugin: this.newVault.authenticationPlugin || null,
      authenticationScope: this.newVault.authenticationScope || null
    }, { headers: this.headers(), withCredentials: true }).subscribe({
      next: () => {
        this.newVault = { name: '', vaultGuid: '', cloudHost: '', organization: '', environment: 'DEV', authenticationMode: 'None', credentialSecretUri: '', authenticationPlugin: '', authenticationScope: '', healthCheckIntervalSeconds: 60, metricsCollectionIntervalMinutes: 15 };
        this.busy = false; this.refreshAll();
      },
      error: e => { this.errorMessage = e.error?.message || 'Impossible d’enregistrer le coffre.'; this.busy = false; }
    });
  }
  testConnection(vault: Vault): void {
    this.busy = true; this.errorMessage = '';
    this.http.post(`/api/vaults/${vault.id}/connection-test`, {}, { headers: this.headers(), withCredentials: true }).subscribe({
      next: () => { this.busy = false; this.refreshAll(); },
      error: e => { this.errorMessage = e.error?.message || 'Le test gRPC a échoué.'; this.busy = false; }
    });
  }
  updateIncident(incident: Incident, status: string): void {
    this.http.patch(`/api/monitoring/incidents/${incident.id}`, { status }, { headers: this.headers(), withCredentials: true }).subscribe({
      next: () => { this.loadIncidents(); this.loadDashboard(); },
      error: () => this.errorMessage = 'Impossible de mettre à jour l’incident.'
    });
  }
  logout(): void {
    this.http.post('/api/auth/logout', {}, { headers: this.headers(), withCredentials: true }).subscribe({
      next: () => { this.authenticated = false; this.vaults = []; this.dashboard = null; this.incidents = []; this.loginPassword = ''; },
      error: () => this.errorMessage = 'La déconnexion a échoué.'
    });
  }
}
