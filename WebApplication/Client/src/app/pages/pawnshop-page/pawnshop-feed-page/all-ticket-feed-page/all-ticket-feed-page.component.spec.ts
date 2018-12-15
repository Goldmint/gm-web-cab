import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { AllTicketFeedPageComponent } from './all-ticket-feed-page.component';

describe('AllTicketFeedPageComponent', () => {
  let component: AllTicketFeedPageComponent;
  let fixture: ComponentFixture<AllTicketFeedPageComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ AllTicketFeedPageComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(AllTicketFeedPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
