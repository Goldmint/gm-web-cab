import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { LaunchNodePageComponent } from './launch-node-page.component';

describe('LaunchNodePageComponent', () => {
  let component: LaunchNodePageComponent;
  let fixture: ComponentFixture<LaunchNodePageComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ LaunchNodePageComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(LaunchNodePageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
