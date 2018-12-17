import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { RewardTransactionsPageComponent } from './reward-transactions-page.component';

describe('RewardTransactionsPageComponent', () => {
  let component: RewardTransactionsPageComponent;
  let fixture: ComponentFixture<RewardTransactionsPageComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ RewardTransactionsPageComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(RewardTransactionsPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
