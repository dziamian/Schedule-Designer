import { Component, Inject, OnInit } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { AccessToken } from 'src/app/others/AccessToken';

import { UsosApiService } from 'src/app/services/UsosApiService/usos-api.service';

@Component({
  selector: 'app-authenticated',
  templateUrl: './authenticated.component.html',
  styleUrls: ['./authenticated.component.css']
})
export class AuthenticatedComponent implements OnInit {

  constructor(private router:Router, private activatedRoute: ActivatedRoute, private usosApiService:UsosApiService) { }

  ngOnInit(): void {
    this.activatedRoute.queryParams.subscribe(params => {
      let oauth_verifier:string = params['oauth_verifier'];
      
      if (oauth_verifier == undefined) {
        return;
      }

      this.usosApiService.AccessToken(oauth_verifier).subscribe(
        token => {
          AccessToken.RemoveRequest();
          token.Save();
          
          this.router.navigate(['profile']);
        },
        response => {
          console.log(response);
          this.router.navigate(['login']);
        }
      )
    })
  }

}
