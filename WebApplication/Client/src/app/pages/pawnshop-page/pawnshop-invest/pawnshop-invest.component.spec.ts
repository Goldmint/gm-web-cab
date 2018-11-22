import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { PawnshopInvestComponent } from './pawnshop-invest.component';

describe('PawnshopInvestComponent', () => {
  let component: PawnshopInvestComponent;
  let fixture: ComponentFixture<PawnshopInvestComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ PawnshopInvestComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(PawnshopInvestComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
