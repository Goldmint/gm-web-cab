import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { PawnshopsTableComponent } from './pawnshops-table.component';

describe('PawnshopsTableComponent', () => {
  let component: PawnshopsTableComponent;
  let fixture: ComponentFixture<PawnshopsTableComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ PawnshopsTableComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(PawnshopsTableComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
