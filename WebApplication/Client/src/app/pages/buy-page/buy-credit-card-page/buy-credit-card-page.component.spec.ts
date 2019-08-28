import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { BuyCreditCardPageComponent } from './buy-credit-card-page.component';

describe('BuyCreditCardPageComponent', () => {
  let component: BuyCreditCardPageComponent;
  let fixture: ComponentFixture<BuyCreditCardPageComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ BuyCreditCardPageComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(BuyCreditCardPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
