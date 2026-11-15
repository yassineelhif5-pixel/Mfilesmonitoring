import { CommonModule } from '@angular/common';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';

interface LoginResult { username: string; mustChangePassword: boolean; }
interface Vault {
  id: string; name: string; vaultGuid: string; cloudHost: string;
  organization: string; environment: string; monitoringEnabled: boolean;
  lastHealthCheck?: {
    checkedAtUtc: string; status: string; durationMs: number;
    authenticationStatus: string; message: string;
  } | null;
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
        <div class="spacer"></div>
        <span class="readonly">READ ONLY</span>
        <span class="user" *ngIf="authenticated">{{ username }}</span>
        <button class="ghost" *ngIf="authenticated" (click)="logout()">Déconnexion</button>
      </header>

      <main class="content" *ngIf="!authenticated">
        <section class="login-card">
          <div class="eyebrow">MFCMP · CLOUD MONITORING</div>
          <h1>Connexion au portail</h1>
          <p class="muted">Accès réservé à l'équipe de supervision M-Files.</p>
          <form (ngSubmit)="login()">
            <label for="username">Identifiant</label>
            <input id="username" name="username" [(ngModel)]="loginName" autocomplete="username" required>
            <label for="password">Mot de passe</label>
            <input id="password" name="password" type="password" [(ngModel)]="loginPassword" autocomplete="current-password" required>
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
          <div><div class="eyebrow">OVERVIEW</div><h1>Supervision des coffres</h1><p class="muted">Vue des coffres M-Files Cloud enregistrés dans le portail.</p></div>
          <button class="primary" (click)="loadVaults()" [disabled]="busy">Actualiser</button>
        </div>
        <section class="notice"><strong>Mode lecture seule</strong><span>Les connecteurs ne réalisent aucune écriture dans les coffres M-Files.</span></section>
        <section class="panel">
          <div class="panel-heading"><div><h2>Coffres surveillés</h2><p class="muted small">Le contrôle REST valide la disponibilité du endpoint, pas l'authentification M-Files.</p></div><span class="count">{{ vaults.length }} coffre(s)</span></div>
          <div class="empty" *ngIf="vaults.length === 0">Aucun coffre configuré. Enregistre un premier coffre ci-dessous.</div>
          <div class="table-wrap" *ngIf="vaults.length > 0">
            <table><thead><tr><th>Coffre</th><th>Environnement</th><th>État</th><th>Temps</th><th>Authentification</th><th></th></tr></thead>
              <tbody><tr *ngFor="let vault of vaults">
                <td><strong>{{ vault.name }}</strong><span class="sub">{{ vault.cloudHost }}</span></td>
                <td>{{ vault.environment }}</td>
                <td><span class="status" [class.ok]="vault.lastHealthCheck?.status === 'REACHABLE_AUTH_NOT_TESTED'">{{ vault.lastHealthCheck?.status || 'NOT_TESTED' }}</span></td>
                <td>{{ vault.lastHealthCheck?.durationMs ?? '—' }}<span *ngIf="vault.lastHealthCheck"> ms</span></td>
                <td>{{ vault.lastHealthCheck?.authenticationStatus || 'NOT_TESTED' }}</td>
                <td><button class="small-button" (click)="testConnection(vault)" [disabled]="busy">Tester</button></td>
              </tr></tbody>
            </table>
          </div>
        </section>
        <section class="panel">
          <h2>Ajouter un coffre Cloud</h2>
          <form class="vault-form" (ngSubmit)="addVault()">
            <div><label for="vaultName">Nom du coffre</label><input id="vaultName" name="vaultName" [(ngModel)]="newVault.name" required></div>
            <div><label for="vaultGuid">Vault GUID</label><input id="vaultGuid" name="vaultGuid" [(ngModel)]="newVault.vaultGuid" required></div>
            <div><label for="cloudHost">Cloud host (HTTPS)</label><input id="cloudHost" name="cloudHost" [(ngModel)]="newVault.cloudHost" placeholder="https://tenant.cloudvault.m-files.com" required></div>
            <div><label for="organization">Organisation</label><input id="organization" name="organization" [(ngModel)]="newVault.organization"></div>
            <div><label for="environment">Environnement</label><select id="environment" name="environment" [(ngModel)]="newVault.environment"><option>DEV</option><option>TEST</option><option>UAT</option><option>PREPROD</option><option>PROD</option></select></div>
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
  newVault = { name: '', vaultGuid: '', cloudHost: '', organization: '', environment: 'DEV' };

  ngOnInit(): void {
    this.http.get<{ requestToken: string }>('/api/auth/csrf', { withCredentials: true }).subscribe({
      next: result => { this.csrfToken = result.requestToken; this.restoreSession(); },
      error: () => this.errorMessage = 'Impossible de contacter le backend. Vérifie que l’API est démarrée.'
    });
  }
  private headers(): HttpHeaders { return new HttpHeaders({ 'X-CSRF-TOKEN': this.csrfToken }); }
  private restoreSession(): void {
    this.http.get<LoginResult>('/api/auth/me', { withCredentials: true }).subscribe({
      next: r => { this.authenticated = true; this.username = r.username; this.mustChangePassword = r.mustChangePassword; if (!this.mustChangePassword) this.loadVaults(); },
      error: () => this.authenticated = false
    });
  }
  login(): void {
    this.busy = true; this.errorMessage = '';
    this.http.post<LoginResult>('/api/auth/login', { username: this.loginName, password: this.loginPassword }, { headers: this.headers(), withCredentials: true }).subscribe({
      next: r => { this.authenticated = true; this.username = r.username; this.mustChangePassword = r.mustChangePassword; this.busy = false; if (!this.mustChangePassword) this.loadVaults(); },
      error: e => { this.errorMessage = e.error?.message || 'Échec de connexion.'; this.busy = false; }
    });
  }
  changePassword(): void {
    this.busy = true; this.errorMessage = '';
    this.http.post('/api/auth/change-password', { currentPassword: this.currentPassword, newPassword: this.newPassword, confirmPassword: this.confirmPassword }, { headers: this.headers(), withCredentials: true }).subscribe({
      next: () => { this.mustChangePassword = false; this.busy = false; this.loadVaults(); },
      error: e => { this.errorMessage = e.error?.errors?.join(' ') || e.error?.message || 'Impossible de modifier le mot de passe.'; this.busy = false; }
    });
  }
  loadVaults(): void {
    this.busy = true;
    this.http.get<Vault[]>('/api/vaults', { withCredentials: true }).subscribe({
      next: r => { this.vaults = r; this.busy = false; },
      error: () => { this.errorMessage = 'Impossible de charger les coffres.'; this.busy = false; }
    });
  }
  addVault(): void {
    this.busy = true; this.errorMessage = '';
    this.http.post('/api/vaults', { ...this.newVault, monitoringEnabled: true }, { headers: this.headers(), withCredentials: true }).subscribe({
      next: () => { this.newVault = { name: '', vaultGuid: '', cloudHost: '', organization: '', environment: 'DEV' }; this.loadVaults(); },
      error: e => { this.errorMessage = e.error?.message || 'Impossible d’enregistrer le coffre.'; this.busy = false; }
    });
  }
  testConnection(vault: Vault): void {
    this.busy = true;
    this.http.post(`/api/vaults/${vault.id}/connection-test`, {}, { headers: this.headers(), withCredentials: true }).subscribe({
      next: () => this.loadVaults(),
      error: e => { this.errorMessage = e.error?.message || 'Le test de connexion a échoué.'; this.busy = false; }
    });
  }
  logout(): void {
    this.http.post('/api/auth/logout', {}, { headers: this.headers(), withCredentials: true }).subscribe({
      next: () => { this.authenticated = false; this.vaults = []; this.loginPassword = ''; },
      error: () => this.errorMessage = 'La déconnexion a échoué.'
    });
  }
}
