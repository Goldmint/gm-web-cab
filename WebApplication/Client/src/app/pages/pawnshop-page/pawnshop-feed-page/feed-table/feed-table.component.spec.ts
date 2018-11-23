import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { FeedTableComponent } from './feed-table.component';

describe('FeedTableComponent', () => {
  let component: FeedTableComponent;
  let fixture: ComponentFixture<FeedTableComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ FeedTableComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(FeedTableComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
