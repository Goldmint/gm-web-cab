import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component, OnDestroy,
  OnInit,
  ViewChild,
  ViewEncapsulation
} from '@angular/core';
import {APIService, MessageBoxService} from "../../services";
import { JsonEditorComponent, JsonEditorOptions } from 'ang-jsoneditor';
import {Subscription} from "rxjs/Subscription";
import {Observable} from "rxjs/Observable";

@Component({
  selector: 'app-telemetry-page',
  templateUrl: './telemetry-page.component.html',
  styleUrls: ['./telemetry-page.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TelemetryPageComponent implements OnInit, OnDestroy {

  public config: object = {};
  public telemetry: object = {};
  private interval: Subscription;

  @ViewChild('configEditor') configEditor: JsonEditorComponent;
  @ViewChild('telemetryEditor') telemetryEditor: JsonEditorComponent;

  public editorOptionsTelemetry: JsonEditorOptions;
  public editorOptionsConfig: JsonEditorOptions;

  constructor(
    private apiService: APIService,
    private cdRef: ChangeDetectorRef,
    private messageBox: MessageBoxService,
  ) {
    this.editorOptionsTelemetry = new JsonEditorOptions();
    this.editorOptionsConfig = new JsonEditorOptions();
    this.editorOptionsTelemetry.mode = 'view';
    this.editorOptionsConfig.mode = 'tree';
  }

  ngOnInit() {
    Observable.combineLatest(
      this.apiService.getConfig(),
      this.apiService.telemetry()
    ).subscribe(data => {
      this.config = JSON.parse(data[0].data.config);
      this.telemetry = JSON.parse(data[1].data.aggregated);

      this.configEditor['editor'].set(this.config);
      this.configEditor['editor'].expandAll();
      this.telemetryEditor['editor'].set(this.telemetry);
      this.telemetryEditor['editor'].expandAll();

      this.cdRef.markForCheck();
    }, () => {
      this.errorFn();
    });

    this.interval = Observable.interval(3000).subscribe(() => {
      this.getTelemetry();
    });
  }

  getTelemetry() {
    this.apiService.telemetry().subscribe(data => {
      const telemetryStr = data.data.aggregated;

      if (telemetryStr !== JSON.stringify(this.telemetry)) {
        this.telemetry = JSON.parse(telemetryStr);

        this.telemetryEditor['editor'].set(this.telemetry);
        this.telemetryEditor['editor'].node.expanded && this.telemetryEditor['editor'].expandAll();

        this.cdRef.markForCheck();
      }
    });
  }

  setConfig() {
    this.config = this.configEditor['editor'].get();
    const config = JSON.stringify(this.config);
    this.apiService.setConfig(config).subscribe(() => {
      this.messageBox.alert('Saved');
    }, () => {
      this.errorFn();
    });
  }

  errorFn() {
    this.messageBox.alert('Error. Something went wrong.');
  };

  ngOnDestroy() {
    this.interval && this.interval.unsubscribe();
  }
}
