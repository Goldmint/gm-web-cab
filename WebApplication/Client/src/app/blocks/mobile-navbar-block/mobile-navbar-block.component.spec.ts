import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { MobileNavbarBlockComponent } from './mobile-navbar-block.component';

describe('MobileNavbarBlockComponent', () => {
  let component: MobileNavbarBlockComponent;
  let fixture: ComponentFixture<MobileNavbarBlockComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ MobileNavbarBlockComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(MobileNavbarBlockComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
