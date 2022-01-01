import { Component, OnInit } from '@angular/core';
import { Coordinator } from 'src/app/others/Accounts';
import { Group } from 'src/app/others/Group';
import { Room } from 'src/app/others/Room';

@Component({
  selector: 'app-available-resources',
  templateUrl: './available-resources.component.html',
  styleUrls: ['./available-resources.component.css']
})
export class AvailableResourcesComponent implements OnInit {

  loading: boolean | null = null;

  coordinators:Coordinator[];
  groups:Group[];
  rooms:Room[];

  constructor() { }

  ngOnInit(): void {
  }

}
