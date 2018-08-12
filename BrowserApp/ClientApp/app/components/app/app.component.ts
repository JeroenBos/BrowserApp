import { Component, Inject, ComponentFactoryResolver, ComponentFactory, OnInit, PACKAGE_ROOT_URL, AfterViewInit } from '@angular/core';
import { Http } from '@angular/http';
import { ChangesPropagator, IComponent } from '../changesPropagator/ChangesPropagator';
import { CommandManager } from '../../commands/commands';

@Component({
    selector: 'app',
    templateUrl: './app.component.html',
    styleUrls: ['./app.component.css']
})
export class AppComponent {
    public readonly changesPropagator: ChangesPropagator;
    constructor(http: Http,
        componentFactoryResolver: ComponentFactoryResolver,
        @Inject('BASE_URL') baseUrl: string) {

        this.changesPropagator = new ChangesPropagator(http, componentFactoryResolver, baseUrl, AppComponent.initialComponents);

        this.changesPropagator.open();
    }

    private static * initialComponents(): Iterable<IComponent> {
        yield new CommandManager(0);
    }
}

