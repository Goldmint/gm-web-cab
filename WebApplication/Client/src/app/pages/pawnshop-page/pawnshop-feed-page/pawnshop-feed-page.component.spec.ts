import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { PawnshopFeedPageComponent } from './pawnshop-feed-page.component';

describe('PawnshopFeedPageComponent', () => {
  let component: PawnshopFeedPageComponent;
  let fixture: ComponentFixture<PawnshopFeedPageComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ PawnshopFeedPageComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(PawnshopFeedPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
