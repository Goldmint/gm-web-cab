import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { SellCryptocurrencyPageComponent } from './sell-cryptocurrency-page.component';

describe('SellCryptocurrencyPageComponent', () => {
  let component: SellCryptocurrencyPageComponent;
  let fixture: ComponentFixture<SellCryptocurrencyPageComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ SellCryptocurrencyPageComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(SellCryptocurrencyPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
