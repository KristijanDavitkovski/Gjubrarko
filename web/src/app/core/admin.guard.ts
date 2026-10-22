import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export const moderatorGuard: CanActivateFn = () =>
  inject(AuthService).isModerator() || inject(Router).createUrlTree(['/login']);
