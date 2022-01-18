import { Component, OnInit } from '@angular/core';
import { ScheduleDesignerApiService } from 'src/app/services/ScheduleDesignerApiService/schedule-designer-api.service';

@Component({
  selector: 'app-administrator-panel',
  templateUrl: './administrator-panel.component.html',
  styleUrls: ['./administrator-panel.component.css']
})
export class AdministratorPanelComponent implements OnInit {

  constructor(
    private scheduleDesignerApiService: ScheduleDesignerApiService,
  ) { }

  ngOnInit(): void {

  }

  public DownloadSchedule() {
    this.scheduleDesignerApiService.DownloadSchedule().subscribe((response) => {
      const blob = new Blob([response.body], { type: "text/csv" });
      const fileName = response.headers.get('Content-Disposition').split(';')[1].trim().split('=')[1];
      const objectUrl = URL.createObjectURL(blob);
      const a = document.createElement('a') as HTMLAnchorElement;

      a.href = objectUrl;
      a.download = fileName;
      document.body.appendChild(a);
      
      a.click();
      
      document.body.removeChild(a);
      URL.revokeObjectURL(objectUrl);
    });
  }
}
