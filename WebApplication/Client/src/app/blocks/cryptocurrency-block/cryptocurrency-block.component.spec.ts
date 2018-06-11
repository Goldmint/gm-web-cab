import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { CryptocurrencyBlockComponent } from './cryptocurrency-block.component';

describe('CryptocurrencyBlockComponent', () => {
  let component: CryptocurrencyBlockComponent;
  let fixture: ComponentFixture<CryptocurrencyBlockComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ CryptocurrencyBlockComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(CryptocurrencyBlockComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
