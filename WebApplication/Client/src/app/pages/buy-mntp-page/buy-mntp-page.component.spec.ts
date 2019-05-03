import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { BuyMntpPageComponent } from './buy-mntp-page.component';

describe('BuyMntpPageComponent', () => {
  let component: BuyMntpPageComponent;
  let fixture: ComponentFixture<BuyMntpPageComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ BuyMntpPageComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(BuyMntpPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
