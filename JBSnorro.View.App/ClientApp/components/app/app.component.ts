import { Component, Inject } from "@angular/core";
import { AppBaseComponent } from "../..";
import { Http } from "@angular/http";

@Component({
    selector: 'app.default',
    templateUrl: './app.component.html',
    styleUrls: ['./app.component.css']
})
export class AppComponent extends AppBaseComponent {
    constructor(http: Http, @Inject('BASE_URL') baseUrl: string) {
        super(http, baseUrl)

        this.server.open();
    }
}