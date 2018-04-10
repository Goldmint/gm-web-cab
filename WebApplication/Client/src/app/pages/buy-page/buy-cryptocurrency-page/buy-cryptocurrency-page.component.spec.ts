import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { BuyCryptocurrencyPageComponent } from './buy-cryptocurrency-page.component';

describe('BuyCryptocurrencyPageComponent', () => {
  let component: BuyCryptocurrencyPageComponent;
  let fixture: ComponentFixture<BuyCryptocurrencyPageComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ BuyCryptocurrencyPageComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(BuyCryptocurrencyPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
