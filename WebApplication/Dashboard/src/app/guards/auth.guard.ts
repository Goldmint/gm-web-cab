import { Injectable } from '@angular/core';
import { Router, CanActivate, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';

import { UserService } from '../services';

@Injectable()
export class AuthGuard implements CanActivate {

  constructor(private router: Router, private userService: UserService) { }

  canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): boolean {
    if (this.userService.isAuthenticated()) {
      return true;
    }

    // localStorage.removeItem('gmint_token');
    this.router.navigate(['/signin'], { queryParams: { returnUrl: state.url }});

    return false;
  }

}
