import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { HoldTokensPageComponent } from './hold-tokens-page.component';

describe('HoldTokensPageComponent', () => {
  let component: HoldTokensPageComponent;
  let fixture: ComponentFixture<HoldTokensPageComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ HoldTokensPageComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(HoldTokensPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
