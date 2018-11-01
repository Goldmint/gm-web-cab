import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { PawnMarketplacePageComponent } from './pawn-marketplace-page.component';

describe('PawnMarketplacePageComponent', () => {
  let component: PawnMarketplacePageComponent;
  let fixture: ComponentFixture<PawnMarketplacePageComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ PawnMarketplacePageComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(PawnMarketplacePageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
