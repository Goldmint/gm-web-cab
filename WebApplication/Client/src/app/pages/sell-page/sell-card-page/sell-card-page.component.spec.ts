import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { SellCardPageComponent } from './sell-card-page.component';

describe('SellCardPageComponent', () => {
  let component: SellCardPageComponent;
  let fixture: ComponentFixture<SellCardPageComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ SellCardPageComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(SellCardPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
