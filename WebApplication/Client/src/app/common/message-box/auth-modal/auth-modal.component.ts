import { Component, OnInit } from '@angular/core';
import {BsModalRef} from "ngx-bootstrap/modal";
import {Router} from "@angular/router";

@Component({
  selector: 'app-auth-modal',
  templateUrl: './auth-modal.component.html',
  styleUrls: ['./auth-modal.component.sass']
})
export class AuthModalComponent implements OnInit {

  constructor(
    private _bsModalRef: BsModalRef,
    private router: Router
  ) { }

  ngOnInit() { }

  hide() {
    this._bsModalRef.hide();
  }

  signIn() {
    this.router.navigate(['/signin']);
    this._bsModalRef.hide();
  }

  signUp() {
    this.router.navigate(['/signup']);
    this._bsModalRef.hide();
  }

}
