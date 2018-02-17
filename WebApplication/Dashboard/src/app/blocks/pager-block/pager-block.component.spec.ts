import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { PagerBlockComponent } from './pager-block.component';

describe('PagerBlockComponent', () => {
  let component: PagerBlockComponent;
  let fixture: ComponentFixture<PagerBlockComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ PagerBlockComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(PagerBlockComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
