import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { BuySepaPageComponent } from './buy-sepa-page.component';

describe('BuySepaPageComponent', () => {
  let component: BuySepaPageComponent;
  let fixture: ComponentFixture<BuySepaPageComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ BuySepaPageComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(BuySepaPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
