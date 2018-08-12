import { Inject, ComponentFactoryResolver, ComponentFactory } from '@angular/core';
import { Http } from '@angular/http';
import { forEach } from '@angular/router/src/utils/collection';
import 'rxjs/add/operator/toPromise';
import { ChangesPropagator, IComponent } from '../components/changesPropagator/ChangesPropagator';

export class Command {
    public constructor(
        public readonly commandId: number,
        public readonly viewModelTypeIds: number[]) {
    }
}
export class CommandInstruction {
    public constructor(
        public readonly commandId: number,
        public readonly viewModelId: number,
        public readonly eventArgs: any) {
    }
}

export class CommandManager implements IComponent {
    [propertyName: string]: any;
    __id: number;

    public constructor(id: number) {
        this.__id = id;
    }
}