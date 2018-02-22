import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { AccessRightsPageComponent } from './access-rights-page.component';

describe('AccessRightsPageComponent', () => {
  let component: AccessRightsPageComponent;
  let fixture: ComponentFixture<AccessRightsPageComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ AccessRightsPageComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(AccessRightsPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
