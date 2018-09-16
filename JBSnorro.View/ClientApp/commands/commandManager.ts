import { Component } from '../angular.index';
import 'rxjs/add/operator/toPromise';
import { BaseViewModel, ICommandManager, BaseComponent } from '../base.component';
import { CommandInstruction } from './commandInstruction';
import { CommandBindingWithCommandName, CommandOptimization, EventToCommandPropagation, DefaultEventArgPropagations, CommandViewModel } from './commands';
import { ConditionAST } from './ConditionAST'
import { CanonicalInputBinding, Kind } from './inputBindingParser';
import { InputEvent, CommandArgs } from './inputTypes';


@Component({
    selector: 'commandmanager',
    templateUrl: './commandManager.html',
    styleUrls: []
})
export class CommandManager extends BaseComponent<CommandManagerViewModel> implements ICommandManager {
    // @ts-ignore: server has no initializer

    public get flags() {
        return this.viewModel.flags;
    }
    public get commands() {
        return this.viewModel.commands;
    }
    public get inputBindings() {
        return this.viewModel.inputBindings;
    }
    public get id() {
        return this.viewModel.__id;
    }

    private readonly commandOptimizations = new Map<string, CommandOptimization>();
    private readonly commandEventPropagations = new Map<string, EventToCommandPropagation>();

    public optimizeCommandClientside(commandName: string, command: CommandOptimization) {
        if (commandName == null || commandName == '') {
            throw new Error('Invalid command name specified');
        }

        this.commandOptimizations.set(commandName, command);
    }
    public setEventArgPropagation(commandName: string, propagation: EventToCommandPropagation) {
        if (commandName == null || commandName == '') {
            throw new Error('Invalid command name specified');
        }
        if (!this.hasCommand(commandName)) {
            throw new Error(`No command with the name '${commandName}' exists`);
        }

        this.commandEventPropagations.set(commandName, propagation);
    }
    public bind(commandName: string, inputBinding: string, condition: string = "") {
        const commandBinding = this.commands[commandName];
        if (commandBinding === undefined) {
            throw new Error(`The command '${name}' is not registered at the command manager`);
        }

        const canonicalInput = CanonicalInputBinding.parse(inputBinding);
        const conditionAST = ConditionAST.parse(condition, this.flags);

        const hasExistingBindingForThisInput = this.inputBindings.has(canonicalInput);
        if (!hasExistingBindingForThisInput) {
            this.inputBindings.set(canonicalInput, []);
        }
        this.inputBindings.get(canonicalInput)!.push(new CommandBindingWithCommandName(commandName, conditionAST, canonicalInput));

    }
    public hasCommand(name: string): boolean {
        return this.commands[name] !== undefined
            && !this.commandOptimizations.has(name); // if this is true, then only a clientside command exists.
    }

    /**
       * 
       * @param sender
       * @param commandName
       * @param e Optional event args, which is consumed if specified (i.e. propagation is stopped).
       */
    public executeCommandByName(commandName: string, sender: BaseViewModel, e?: InputEvent): void {
        const executed = this.executeCommandIfPossible(commandName, sender, e);
        if (!executed && this.hasCommand(commandName)) {
            console.warn(`The command '${commandName}' cannot execute on '${Object.getPrototypeOf(this).constructor.name}'(id=${sender.__id})`);
        }
    }
    public handleMouseMove(sender: BaseViewModel, e: MouseEvent): void {

        this.handle(CanonicalInputBinding.fromMouseMoveEvent(e), sender, e);
    }
    public handleMouseClick(sender: BaseViewModel, e: MouseEvent): void {
        // in javascript a mouse click is a left mouse button down and up event together, triggered at the moment of up event
        // for simplicity I simply trigger another kind of event, but I think the canonical input 'click' could be inferred from up and down events
        this.handle(CanonicalInputBinding.fromMouseEvent(e, Kind.Click), sender, e);
    }
    public handleMouseDown(sender: BaseViewModel, e: MouseEvent): void {
        this.handle(CanonicalInputBinding.fromMouseEvent(e, Kind.Down), sender, e);
    }
    public handleMouseUp(sender: BaseViewModel, e: MouseEvent): void {
        this.handle(CanonicalInputBinding.fromMouseEvent(e, Kind.Up), sender, e);
    }
    public handleKeypress(sender: BaseViewModel, e: KeyboardEvent): void {
        // key up events aren't handled (yet)
        this.handle(CanonicalInputBinding.fromKeyboardEvent(e), sender, e);
    }

    private handle(
        inputBinding: CanonicalInputBinding,
        sender: BaseViewModel,
        e: InputEvent): void {

        const commandNames = this.getCommandBindingsFor(inputBinding, sender, e);

        for (let i = 0; i < commandNames.length; i++) {

            const executed = this.executeCommandIfPossible(commandNames[0], sender, e);
            if (executed) {
                e.stopPropagation();
                // decide here whether to invoke all executable bound commands, or merely the first one, or dependent on properties of InputEvent 
                break;
            }
        }

    }

    /**
     * Gets the names of the commands bound to the specified input, for which the binding condition is true.
     */
    private getCommandBindingsFor(inputBinding: CanonicalInputBinding, sender: BaseViewModel, e: InputEvent): string[] {
        const commandBindings = this.inputBindings.get(inputBinding);
        if (commandBindings === undefined) {
            return [];
        }

        const commandNames: string[] = [];

        commandBindings.forEach((binding: CommandBindingWithCommandName) => {
            if (binding.condition.toBoolean(sender, e)) {
                commandNames.push(binding.commandName);
            }
        });

        return commandNames;
    }

    private executeCommandIfPossible(commandName: string, sender: BaseViewModel, e?: InputEvent): boolean {

        const args = this.getEventArgs(commandName, sender, e);
        const serverSideExecuted = this.executeServersideCommandIfPossible(commandName, sender, args, e);
        const clientSideExecuted = this.executeClientsideCommandIfPossible(commandName, sender, args);

        return serverSideExecuted || clientSideExecuted;
    }
    private getEventArgs(commandName: string, sender: BaseViewModel, e?: InputEvent): any {
        const propagation = this.commandEventPropagations.get(commandName);
        if (propagation === undefined) {
            return undefined;
        }

        if (DefaultEventArgPropagations.IsInstanceOf(propagation)) {
            return DefaultEventArgPropagations.GetDefault(propagation)(e);
        }
        else {
            return propagation(e);
        }
    }
    private executeServersideCommandIfPossible(commandName: string, sender: BaseViewModel, args: CommandArgs, e: InputEvent | undefined): boolean {

        const command = this.commands[commandName];
        if (command === undefined) {
            if (this.commandOptimizations.get(commandName) === undefined) {
                console.warn(`The command '${commandName}' does not exist`);
            }
            return false;
        }

        if (command.condition !== undefined && ConditionAST.parse(command.condition, this.flags).toBoolean(sender, e)) {
            return false;
        }

        this.server.executeCommand(new CommandInstruction(commandName, sender, args));
        return true;
    }
    private executeClientsideCommandIfPossible(commandName: string, sender: BaseViewModel, args: CommandArgs): boolean {
        const command = this.commandOptimizations.get(commandName);
        if (command === undefined) {
            return false;
        }

        if (!command.canExecute(sender, args)) {
            return false;
        }

        command.execute(sender, args);
        return true;
    }
}





export interface CommandManagerViewModel extends BaseViewModel {
    flags: Map<string, boolean>;
    commands: CommandsMap;
    inputBindings: Map<CanonicalInputBinding, CommandBindingWithCommandName[]>;
}


export interface CommandsMap {
    [s: string]: CommandViewModel;
}
