import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { MasterNodePageComponent } from './master-node-page.component';

describe('MasterNodePageComponent', () => {
  let component: MasterNodePageComponent;
  let fixture: ComponentFixture<MasterNodePageComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ MasterNodePageComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(MasterNodePageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
