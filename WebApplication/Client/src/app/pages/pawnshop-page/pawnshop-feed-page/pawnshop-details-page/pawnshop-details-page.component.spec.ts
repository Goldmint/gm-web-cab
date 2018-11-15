import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { PawnshopDetailsPageComponent } from './pawnshop-details-page.component';

describe('PawnshopDetailsPageComponent', () => {
  let component: PawnshopDetailsPageComponent;
  let fixture: ComponentFixture<PawnshopDetailsPageComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ PawnshopDetailsPageComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(PawnshopDetailsPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
