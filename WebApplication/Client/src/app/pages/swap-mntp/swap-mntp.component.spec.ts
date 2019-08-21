import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { SwapMntpComponent } from './swap-mntp.component';

describe('SwapMntpComponent', () => {
  let component: SwapMntpComponent;
  let fixture: ComponentFixture<SwapMntpComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ SwapMntpComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(SwapMntpComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
