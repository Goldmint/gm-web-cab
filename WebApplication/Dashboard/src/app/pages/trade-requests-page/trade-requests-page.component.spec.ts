import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { TradeRequestsPageComponent } from './trade-requests-page.component';

describe('TradeRequestsPageComponent', () => {
  let component: TradeRequestsPageComponent;
  let fixture: ComponentFixture<TradeRequestsPageComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ TradeRequestsPageComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(TradeRequestsPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
