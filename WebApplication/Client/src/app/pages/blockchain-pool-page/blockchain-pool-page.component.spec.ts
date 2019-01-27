import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { BlockchainPoolPageComponent } from './blockchain-pool-page.component';

describe('BlockchainPoolPageComponent', () => {
  let component: BlockchainPoolPageComponent;
  let fixture: ComponentFixture<BlockchainPoolPageComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ BlockchainPoolPageComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(BlockchainPoolPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
