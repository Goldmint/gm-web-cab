import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { NoVerificationBlockComponent } from './no-verification-block.component';

describe('NoVerificationBlockComponent', () => {
  let component: NoVerificationBlockComponent;
  let fixture: ComponentFixture<NoVerificationBlockComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ NoVerificationBlockComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(NoVerificationBlockComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
