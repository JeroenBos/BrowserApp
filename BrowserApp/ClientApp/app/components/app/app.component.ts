import { Component, Inject, ComponentFactoryResolver, ComponentFactory, OnInit, PACKAGE_ROOT_URL, AfterViewInit } from '@angular/core';
import { Http } from '@angular/http';
import { ChangesPropagator } from '../changesPropagator/ChangesPropagator';

@Component({
    selector: 'app',
    templateUrl: './app.component.html',
    styleUrls: ['./app.component.css']
})
export class AppComponent {
    private readonly changesPropagator: ChangesPropagator;
    constructor(http: Http,
        componentFactoryResolver: ComponentFactoryResolver,
        @Inject('BASE_URL') baseUrl: string) {

        this.changesPropagator = new ChangesPropagator(http, componentFactoryResolver, baseUrl);

        this.changesPropagator.registerRequest();
    }
}

