import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { SwiftPageComponent } from './swift-page.component';

describe('SwiftPageComponent', () => {
  let component: SwiftPageComponent;
  let fixture: ComponentFixture<SwiftPageComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ SwiftPageComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(SwiftPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
