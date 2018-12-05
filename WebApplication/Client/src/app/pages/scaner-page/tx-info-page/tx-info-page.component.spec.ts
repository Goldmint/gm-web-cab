import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { TxInfoPageComponent } from './tx-info-page.component';

describe('TxInfoPageComponent', () => {
  let component: TxInfoPageComponent;
  let fixture: ComponentFixture<TxInfoPageComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ TxInfoPageComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(TxInfoPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
