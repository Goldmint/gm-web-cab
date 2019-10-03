import { Injectable } from '@angular/core';
import { Router, CanActivate, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { UserService } from '../services';
import {Subscription} from "rxjs";

@Injectable()
export class TosGuard implements CanActivate {

  private sub: Subscription;

  constructor(private router: Router, private userService: UserService) { }

  canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): boolean {
    if (this.userService.isAuthenticated()) {
      this.sub = this.userService.currentUser.subscribe(user => {
        this.sub && this.sub.unsubscribe();
        if (user && user.hasOwnProperty('verifiedL0') && !user.verifiedL0) {
          this.router.navigate(['/tos-verification']);
          return false;
        }
      });
    } else {
      this.sub && this.sub.unsubscribe();
    }

    return true;
  }

}
