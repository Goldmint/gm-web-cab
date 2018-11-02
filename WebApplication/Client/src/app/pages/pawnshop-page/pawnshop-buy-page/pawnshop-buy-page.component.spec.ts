import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { PawnshopBuyPageComponent } from './pawnshop-buy-page.component';

describe('PawnshopBuyPageComponent', () => {
  let component: PawnshopBuyPageComponent;
  let fixture: ComponentFixture<PawnshopBuyPageComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ PawnshopBuyPageComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(PawnshopBuyPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
