import {
  ChangeDetectionStrategy, ChangeDetectorRef,
  Component,
  OnInit,
  ViewEncapsulation
} from '@angular/core';
import {UserService} from "../../../../services";
import {Subject} from "rxjs/Subject";

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
  public stepperData;
  public isDenied: boolean = true;

  private destroy$: Subject<boolean> = new Subject<boolean>();

  constructor(
    private userService: UserService,
    private cdRef: ChangeDetectorRef
  ) { }

  ngOnInit() {
    this.userService.organizationStepper$.takeUntil(this.destroy$).subscribe((data: any) => {
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
    this.stepperData.step = this.currentStep;;
    this.stepperData.id = this.currentId;
    this.cdRef.markForCheck();
  }

  ngOnDestroy() {
    this.userService.organizationStepper$.next(null);
    this.destroy$.next(true);
  }
}
