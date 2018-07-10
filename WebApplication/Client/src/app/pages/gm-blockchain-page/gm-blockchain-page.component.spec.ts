import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { GmBlockchainPageComponent } from './gm-blockchain-page.component';

describe('GmBlockchainPageComponent', () => {
  let component: GmBlockchainPageComponent;
  let fixture: ComponentFixture<GmBlockchainPageComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ GmBlockchainPageComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(GmBlockchainPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
