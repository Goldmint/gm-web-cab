import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { TransactionsInBlockPageComponent } from './transactions-in-block-page.component';

describe('TransactionsInBlockPageComponent', () => {
  let component: TransactionsInBlockPageComponent;
  let fixture: ComponentFixture<TransactionsInBlockPageComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ TransactionsInBlockPageComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(TransactionsInBlockPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
