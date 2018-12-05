import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { ScanerPageComponent } from './scaner-page.component';

describe('ScanerPageComponent', () => {
  let component: ScanerPageComponent;
  let fixture: ComponentFixture<ScanerPageComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ ScanerPageComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ScanerPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
