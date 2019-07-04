import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { NetworkSwitcherBlockComponent } from './network-switcher-block.component';

describe('NetworkSwitcherBlockComponent', () => {
  let component: NetworkSwitcherBlockComponent;
  let fixture: ComponentFixture<NetworkSwitcherBlockComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ NetworkSwitcherBlockComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(NetworkSwitcherBlockComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
