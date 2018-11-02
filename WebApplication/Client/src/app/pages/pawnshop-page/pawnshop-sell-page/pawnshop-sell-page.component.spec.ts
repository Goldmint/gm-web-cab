import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { PawnshopSellPageComponent } from './pawnshop-sell-page.component';

describe('PawnshopSellPageComponent', () => {
  let component: PawnshopSellPageComponent;
  let fixture: ComponentFixture<PawnshopSellPageComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ PawnshopSellPageComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(PawnshopSellPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
