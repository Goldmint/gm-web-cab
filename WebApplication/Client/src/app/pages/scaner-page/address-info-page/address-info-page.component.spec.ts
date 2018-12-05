import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { AddressInfoPageComponent } from './address-info-page.component';

describe('AddressInfoPageComponent', () => {
  let component: AddressInfoPageComponent;
  let fixture: ComponentFixture<AddressInfoPageComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ AddressInfoPageComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(AddressInfoPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
