import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { PromoCodesInfoPageComponent } from './promo-codes-info-page.component';

describe('PromoCodesInfoPageComponent', () => {
  let component: PromoCodesInfoPageComponent;
  let fixture: ComponentFixture<PromoCodesInfoPageComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ PromoCodesInfoPageComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(PromoCodesInfoPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
