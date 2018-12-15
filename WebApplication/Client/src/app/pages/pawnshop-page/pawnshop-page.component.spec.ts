import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { PawnshopPageComponent } from './pawnshop-page.component';

describe('PawnshopPageComponent', () => {
  let component: PawnshopPageComponent;
  let fixture: ComponentFixture<PawnshopPageComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ PawnshopPageComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(PawnshopPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
