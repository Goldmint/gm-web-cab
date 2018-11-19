import {
  ChangeDetectionStrategy, ChangeDetectorRef,
  Component,
  OnInit,
  ViewEncapsulation
} from '@angular/core';
import {Subject} from "rxjs/Subject";
import {CommonService} from "../../../../services/common.service";
import {OrgStepperData} from "../../../../models/org-stepper-data";

@Component({
  selector: 'app-organizations-page',
  templateUrl: './organizations-page.component.html',
  styleUrls: ['./organizations-page.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class OrganizationsPageComponent implements OnInit {

  public currentStep: number = 1;
  public currentId: number = null;
  public stepperData: OrgStepperData = new OrgStepperData();
  public isDenied: boolean = true;

  private destroy$: Subject<boolean> = new Subject<boolean>();

  constructor(
    private cdRef: ChangeDetectorRef,
    private commonService: CommonService
  ) { }

  ngOnInit() {
    this.commonService.organizationStepper$.takeUntil(this.destroy$).subscribe((data: OrgStepperData) => {
      if (data !== null) {
        this.stepperData = data;
        this.currentId = data.id;
        this.nextStep();
      }
    });
  }

  prevStep() {
    this.currentStep--;
    this.cdRef.markForCheck();
  }

  nextStep() {
    this.currentStep++;
    this.stepperData.step = this.currentStep;
    this.stepperData.id = this.currentId;
    this.cdRef.markForCheck();
  }

  ngOnDestroy() {
    this.commonService.organizationStepper$.next(null);
    this.commonService.setTwoOrganizationStep$.next(null);
    this.destroy$.next(true);
  }
}
