import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { BuySellGoldPageComponent } from './buy-sell-gold-page.component';

describe('BuySellGoldPageComponent', () => {
  let component: BuySellGoldPageComponent;
  let fixture: ComponentFixture<BuySellGoldPageComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ BuySellGoldPageComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(BuySellGoldPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
