import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { OplogPageComponent } from './oplog-page.component';

describe('OplogPageComponent', () => {
  let component: OplogPageComponent;
  let fixture: ComponentFixture<OplogPageComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ OplogPageComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(OplogPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
