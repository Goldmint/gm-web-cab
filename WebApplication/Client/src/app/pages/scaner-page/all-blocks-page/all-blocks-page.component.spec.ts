import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { AllBlocksPageComponent } from './all-blocks-page.component';

describe('AllBlocksPageComponent', () => {
  let component: AllBlocksPageComponent;
  let fixture: ComponentFixture<AllBlocksPageComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ AllBlocksPageComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(AllBlocksPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
