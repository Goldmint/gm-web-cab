import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { LatestRewardPageComponent } from './latest-reward-page.component';

describe('LatestRewardPageComponent', () => {
  let component: LatestRewardPageComponent;
  let fixture: ComponentFixture<LatestRewardPageComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ LatestRewardPageComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(LatestRewardPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
