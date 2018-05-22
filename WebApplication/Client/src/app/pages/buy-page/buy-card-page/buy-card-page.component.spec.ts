import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { BuyCardPageComponent } from './buy-card-page.component';

describe('BuyCardPageComponent', () => {
  let component: BuyCardPageComponent;
  let fixture: ComponentFixture<BuyCardPageComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ BuyCardPageComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(BuyCardPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
