import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-login',
  imports: [FormsModule],
  templateUrl: './login.html',
  styleUrl: './login.scss'
})
export class Login {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  username = 'admin';
  password = 'admin123';
  readonly loading = signal(false);
  readonly error = signal('');

  submit(): void {
    this.loading.set(true);
    this.error.set('');
    this.auth.login(this.username, this.password).pipe(
      finalize(() => this.loading.set(false))
    ).subscribe({
      next: () => void this.router.navigateByUrl(
        this.route.snapshot.queryParamMap.get('returnUrl') ?? '/dashboard'
      ),
      error: () => this.error.set('Pogrešno korisničko ime ili lozinka.')
    });
  }
}
